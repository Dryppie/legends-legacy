export interface GuideSection {
  heading: string;
  body: string;
}

export interface Guide {
  title: string;
  lastReviewed: string;
  sections: GuideSection[];
}
